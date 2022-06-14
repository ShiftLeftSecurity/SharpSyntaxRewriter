#!groovy
pipeline {
    agent { label 'dotnet-windows' }
    environment {
        REPO_NAME = "github.com/ShiftLeftSecurity/SharpSyntaxRewriter"
        GITHUB_KEY = '4b3482c3-735f-4c31-8d1b-d8d3bd889348'
    }
    options{
        skipDefaultCheckout()
        disableConcurrentBuilds()
    }
    parameters { string(name: 'LIB_TAG', defaultValue: '', description: 'Please enter LIB_TAG value') }
    stages {
        stage('getSrc') {
            steps {
                script {
                    checkout([
                        $class: 'GitSCM',
                        branches: [[name: "*/master"]],
                        extensions: [[
                            $class: 'PathRestriction',
                            excludedRegions: '',
                            includedRegions: ''
                        ]],
                        userRemoteConfigs: [[
                            credentialsId: '4b3482c3-735f-4c31-8d1b-d8d3bd889348',
                            url: "ssh://git@${env.REPO_NAME}"
                        ]]
                    ])
                }
            }
        }
        stage('push-nugget') {
            steps {
                script {
                    sh "git clone git@github.com:ShiftLeftSecurity/SharpSyntaxRewriter.git"
                    sh "git checkout tags/{LIB_TAG} -b dist"
                    sh "dotnet build -c Release src/SharpSyntaxRewriter/SharpSyntaxRewriter.csproj"
                    sh "dotnet pack -c Release src/SharpSyntaxRewriter/SharpSyntaxRewriter.csproj -o ."
                    sh "nuget push 'SharpSyntaxRewriter.${LIB_TAG}.nupkg' -Source Artifactory"
                }
            }
        }
    }
    post {
        failure {
            notifyFailure()
        }
        unstable {
            notifyUnstable()
        }
        success {
            notifySuccess()
        }
    }
}

def notifyFailure() {
    notifySlack('#951D13', 'Failure')
}
def notifySuccess() {
    notifySlack('#5cb589', 'Success')
}
def notifySlack(color, status) {
    //slackSend (channel: "${env.SLACK_NOTIFY_CHANNEL}", color: "${color}", message: "${env.JOB_NAME} - #${env.BUILD_NUMBER} ${status} after ${currentBuild.durationString.replace(' and counting', '')} (<${env.BUILD_URL}|Open>)")
}